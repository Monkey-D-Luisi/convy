"""Public CI identity checks shared by the runner and the installed host broker."""
import json
import os
from pathlib import Path
import urllib.request

from release_common import SHA, ReleaseError, require

REPOSITORY = 'Monkey-D-Luisi/convy'
CI_WORKFLOW_ID = 256590363
SIGNER = REPOSITORY + '/.github/workflows/staging-cd.yml'


def authorize_ci(ci, master, active=None):
    require(ci.get('workflow_id') == CI_WORKFLOW_ID and ci.get('path') == '.github/workflows/ci.yml', 'unauthorized_ci_workflow')
    require(ci.get('head_repository', {}).get('full_name') == REPOSITORY, 'fork_artifact_denied')
    require(ci.get('event') == 'push' and ci.get('head_branch') == 'master', 'non_master_push_denied')
    require(ci.get('status') == 'completed' and ci.get('conclusion') == 'success', 'ci_not_successful')
    sha = ci.get('head_sha', '')
    require(SHA.fullmatch(sha) is not None and sha == master, 'superseded_master_commit')
    require(isinstance(ci.get('id'), int) and ci['id'] > 0 and isinstance(ci.get('run_attempt'), int), 'invalid_ci_identity')
    if active:
        if active['sourceSha'] == sha:
            return 'ALREADY_ACCEPTED'
        require(ci['id'] > active['ciRunId'], 'older_ci_run_denied')
    return 'AUTHORIZED'


def api(path, token=None):
    headers = {'Accept': 'application/vnd.github+json', 'User-Agent': 'convy-staging-controller', 'X-GitHub-Api-Version': '2022-11-28'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    request = urllib.request.Request('https://api.github.com/repos/' + REPOSITORY + '/' + path, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            return json.load(response)
    except Exception:
        raise ReleaseError('github_verification_unavailable') from None


def verify_ci(request, token=None, active=None):
    require(SHA.fullmatch(request.get('sourceSha', '')) is not None, 'exact_source_required')
    require(all(isinstance(request.get(k), int) and request[k] > 0 for k in ('ciRunId', 'ciRunAttempt', 'cdRunId')), 'invalid_run_ids')
    master = api('git/ref/heads/master', token)['object']['sha']
    ci = api('actions/runs/' + str(request['ciRunId']), token)
    status = authorize_ci(ci, master, active)
    require(ci['head_sha'] == request['sourceSha'] and ci['run_attempt'] == request['ciRunAttempt'], 'ci_receipt_mismatch')
    latest = api('actions/workflows/ci.yml/runs?event=push&branch=master&head_sha=' + request['sourceSha'] + '&per_page=1', token)['workflow_runs']
    require(bool(latest) and latest[0]['id'] == ci['id'] and latest[0]['run_attempt'] == ci['run_attempt'], 'superseded_ci_attempt')
    cd = api('actions/runs/' + str(request['cdRunId']), token)
    require(cd.get('event') == 'workflow_run' and cd.get('head_sha') == master and
            cd.get('head_branch') == 'master' and cd.get('path') == '.github/workflows/staging-cd.yml' and
            cd.get('head_repository', {}).get('full_name') == REPOSITORY, 'unauthorized_cd_run')
    return status


def runner_request():
    require(os.environ.get('GITHUB_EVENT_NAME') == 'workflow_run' and os.environ.get('GITHUB_REPOSITORY') == REPOSITORY and
            os.environ.get('GITHUB_REF') == 'refs/heads/master', 'unauthorized_runner_context')
    event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes())
    ci = event['workflow_run']
    request = {'sourceSha': ci['head_sha'], 'ciRunId': ci['id'], 'ciRunAttempt': ci['run_attempt'], 'cdRunId': int(os.environ['GITHUB_RUN_ID'])}
    require(request['sourceSha'] == os.environ['GITHUB_SHA'], 'runner_workflow_source_mismatch')
    verify_ci(request, os.environ.get('GH_TOKEN'))
    return request

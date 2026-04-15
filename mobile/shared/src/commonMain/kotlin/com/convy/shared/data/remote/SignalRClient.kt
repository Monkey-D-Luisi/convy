package com.convy.shared.data.remote

import com.convy.shared.config.ApiConfig
import io.ktor.client.*
import io.ktor.client.plugins.websocket.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.websocket.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.*
import kotlinx.serialization.json.*

enum class ConnectionState {
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}

class SignalRClient(
    private val tokenProvider: TokenProvider,
    private val json: Json,
    private val apiConfig: ApiConfig
) {
    private val baseHost: String get() = apiConfig.host
    private val basePort: Int get() = apiConfig.port
    private val baseProtocol: URLProtocol
        get() = if (apiConfig.protocol == "https") URLProtocol.HTTPS else URLProtocol.HTTP
    private val wsProtocol: URLProtocol
        get() = if (apiConfig.protocol == "https") URLProtocol.WSS else URLProtocol.WS
    private val wsClient = HttpClient {
        install(WebSockets)
    }

    private val httpClient = HttpClient()

    private val _messages = MutableSharedFlow<SignalRMessage>(extraBufferCapacity = 64)
    val messages: SharedFlow<SignalRMessage> = _messages

    private val _connectionState = MutableStateFlow(ConnectionState.Disconnected)
    val connectionState: StateFlow<ConnectionState> = _connectionState.asStateFlow()

    private var session: WebSocketSession? = null
    private var connectionJob: Job? = null
    private val scope = CoroutineScope(Dispatchers.Default + SupervisorJob())

    private var currentHouseholdId: String? = null
    private var explicitDisconnect = false

    suspend fun connect(householdId: String) {
        disconnect()
        explicitDisconnect = false
        currentHouseholdId = householdId

        connectionJob = scope.launch {
            var retryDelay = INITIAL_RETRY_DELAY_MS
            var isFirstAttempt = true

            while (isActive) {
                val token = tokenProvider.getToken()
                if (token == null) {
                    _connectionState.value = ConnectionState.Disconnected
                    return@launch
                }

                _connectionState.value = if (isFirstAttempt) ConnectionState.Connecting else ConnectionState.Reconnecting

                try {
                    connectWebSocket(token, householdId)
                } catch (_: CancellationException) {
                    throw CancellationException()
                } catch (_: Exception) {
                    // Connection failed or dropped
                }

                if (!isActive || explicitDisconnect) break

                // Connection lost — retry with backoff
                _connectionState.value = ConnectionState.Reconnecting
                isFirstAttempt = false
                delay(retryDelay)
                retryDelay = (retryDelay * 2).coerceAtMost(MAX_RETRY_DELAY_MS)
            }

            _connectionState.value = ConnectionState.Disconnected
        }
    }

    private suspend fun connectWebSocket(token: String, householdId: String) {
        // Negotiate to get connectionToken
        val negotiateResponse = httpClient.post {
            url {
                protocol = baseProtocol
                host = baseHost
                port = basePort
                path("hubs", "household", "negotiate")
                parameter("negotiateVersion", "1")
            }
            header("Authorization", "Bearer $token")
        }

        val negotiateBody = negotiateResponse.bodyAsText()
        val negotiateJson = json.parseToJsonElement(negotiateBody).jsonObject
        val connectionToken = negotiateJson["connectionToken"]?.jsonPrimitive?.contentOrNull

        wsClient.webSocket(
            request = {
                url {
                    protocol = wsProtocol
                    host = baseHost
                    port = basePort
                    path("hubs", "household")
                    parameter("access_token", token)
                    if (connectionToken != null) {
                        parameter("id", connectionToken)
                    }
                }
            }
        ) {
            session = this

            // Send handshake
            send(Frame.Text("{\"protocol\":\"json\",\"version\":1}\u001E"))

            // Read handshake response
            val handshake = incoming.receive()
            if (handshake is Frame.Text) {
                val text = handshake.readText().trimEnd('\u001E')
                val parsed = json.parseToJsonElement(text).jsonObject
                if (parsed.containsKey("error")) {
                    return@webSocket
                }
            }

            // Join household group
            sendInvocation("JoinHousehold", listOf(JsonPrimitive(householdId)))

            _connectionState.value = ConnectionState.Connected

            // Listen for messages
            for (frame in incoming) {
                if (frame is Frame.Text) {
                    val text = frame.readText()
                    text.split('\u001E').filter { it.isNotBlank() }.forEach { msg ->
                        processMessage(msg)
                    }
                }
            }
        }
    }

    suspend fun disconnect() {
        explicitDisconnect = true
        connectionJob?.cancel()
        connectionJob = null
        try {
            session?.close()
        } catch (_: Exception) {}
        session = null
        _connectionState.value = ConnectionState.Disconnected
    }

    private suspend fun WebSocketSession.sendInvocation(target: String, arguments: List<JsonElement>) {
        val message = buildJsonObject {
            put("type", 1)
            put("target", target)
            putJsonArray("arguments") {
                arguments.forEach { add(it) }
            }
        }
        send(Frame.Text("${json.encodeToString(JsonObject.serializer(), message)}\u001E"))
    }

    private suspend fun processMessage(raw: String) {
        try {
            val obj = json.parseToJsonElement(raw).jsonObject
            val type = obj["type"]?.jsonPrimitive?.intOrNull ?: return

            when (type) {
                1 -> {
                    val target = obj["target"]?.jsonPrimitive?.contentOrNull ?: return
                    val arguments = obj["arguments"]?.jsonArray ?: return
                    _messages.emit(SignalRMessage(target, arguments))
                }
                6 -> {
                    session?.send(Frame.Text("{\"type\":6}\u001E"))
                }
            }
        } catch (_: Exception) {}
    }

    companion object {
        private const val INITIAL_RETRY_DELAY_MS = 1_000L
        private const val MAX_RETRY_DELAY_MS = 30_000L
    }
}

data class SignalRMessage(
    val target: String,
    val arguments: JsonArray
)

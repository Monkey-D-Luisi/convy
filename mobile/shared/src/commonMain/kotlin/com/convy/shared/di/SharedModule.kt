package com.convy.shared.di

import com.convy.shared.config.ApiConfig
import com.convy.shared.data.offline.OfflineActionQueue
import com.convy.shared.data.offline.SyncManager
import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.DeviceTokenManager
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.data.repository.*
import com.convy.shared.domain.repository.*
import com.convy.shared.platform.FileStorage
import com.convy.shared.platform.NetworkMonitor
import io.ktor.client.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.api.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.client.request.forms.FormDataContent
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
import io.ktor.utils.io.errors.IOException
import kotlinx.serialization.json.Json
import org.koin.dsl.module

val networkModule = module {
    single {
        Json {
            ignoreUnknownKeys = true
            isLenient = true
            encodeDefaults = true
        }
    }

    single {
        val tokenProvider = get<TokenProvider>()
        val apiConfig = get<ApiConfig>()
        val urlProtocol = if (apiConfig.protocol == "https") URLProtocol.HTTPS else URLProtocol.HTTP
        HttpClient {
            install(ContentNegotiation) {
                json(get<Json>())
            }
            install(HttpTimeout) {
                connectTimeoutMillis = 10_000
                requestTimeoutMillis = 30_000
                socketTimeoutMillis = 30_000
            }
            install(HttpRequestRetry) {
                maxRetries = 2
                retryIf { _, response -> response.status.value in 500..599 }
                retryOnExceptionIf { request, cause ->
                    cause is IOException && request.body !is FormDataContent
                }
                delayMillis { retryCount -> (retryCount * 2000L) }
            }
            install(createClientPlugin("FirebaseAuth") {
                onRequest { request, _ ->
                    val token = tokenProvider.getToken()
                    if (token != null) {
                        request.bearerAuth(token)
                    }
                }
            })
            HttpResponseValidator {
                validateResponse { response ->
                    if (!response.status.isSuccess()) {
                        val body = response.bodyAsText()
                        throw ResponseException(response, body)
                    }
                }
            }
            defaultRequest {
                url {
                    protocol = urlProtocol
                    host = apiConfig.host
                    port = apiConfig.port
                }
            }
        }
    }

    single { ConvyApi(get()) }

    single { DeviceTokenManager(get(), get(), get()) }

    single {
        val apiConfig = get<ApiConfig>()
        SignalRClient(get<TokenProvider>(), get<Json>(), apiConfig)
    }
    single { HouseholdRealtimeService(get(), get<Json>()) }

    single { OfflineActionQueue(get<FileStorage>(), get<Json>()) }
    single { SyncManager(get(), get<NetworkMonitor>(), get()) }
}

val repositoryModule = module {
    single { FirebaseAuthRepository(get()) }
    single<AuthRepository> { get<FirebaseAuthRepository>() }
    single<TokenProvider> { get<FirebaseAuthRepository>() }
    single<UserRepository> { UserRepositoryImpl(get()) }
    single<HouseholdRepository> { HouseholdRepositoryImpl(get()) }
    single<ListRepository> { ListRepositoryImpl(get()) }
    single<ItemRepository> { ItemRepositoryImpl(get(), get()) }
    single<TaskRepository> { TaskRepositoryImpl(get()) }
    single<InviteRepository> { InviteRepositoryImpl(get()) }
    single<ActivityRepository> { ActivityRepositoryImpl(get()) }
}

val sharedModules = listOf(networkModule, repositoryModule)

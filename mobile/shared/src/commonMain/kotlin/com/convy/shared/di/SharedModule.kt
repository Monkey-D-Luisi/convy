package com.convy.shared.di

import com.convy.shared.config.ApiConfig
import com.convy.shared.data.remote.ConvyApi
import com.convy.shared.data.remote.DeviceTokenManager
import com.convy.shared.data.remote.HouseholdRealtimeService
import com.convy.shared.data.remote.SignalRClient
import com.convy.shared.data.remote.TokenProvider
import com.convy.shared.data.repository.*
import com.convy.shared.domain.repository.*
import io.ktor.client.*
import io.ktor.client.plugins.*
import io.ktor.client.plugins.auth.*
import io.ktor.client.plugins.auth.providers.*
import io.ktor.client.plugins.contentnegotiation.*
import io.ktor.http.*
import io.ktor.serialization.kotlinx.json.*
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
            install(Auth) {
                bearer {
                    loadTokens {
                        val token = tokenProvider.getToken()
                        if (token != null) BearerTokens(token, "") else null
                    }
                    refreshTokens {
                        val token = tokenProvider.getToken()
                        if (token != null) BearerTokens(token, "") else null
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

    single { DeviceTokenManager(get(), get()) }

    single {
        val apiConfig = get<ApiConfig>()
        SignalRClient(get<TokenProvider>(), get<Json>(), apiConfig)
    }
    single { HouseholdRealtimeService(get(), get<Json>()) }
}

val repositoryModule = module {
    single { FirebaseAuthRepository() }
    single<AuthRepository> { get<FirebaseAuthRepository>() }
    single<TokenProvider> { get<FirebaseAuthRepository>() }
    single<UserRepository> { UserRepositoryImpl(get()) }
    single<HouseholdRepository> { HouseholdRepositoryImpl(get()) }
    single<ListRepository> { ListRepositoryImpl(get()) }
    single<ItemRepository> { ItemRepositoryImpl(get()) }
    single<InviteRepository> { InviteRepositoryImpl(get()) }
    single<ActivityRepository> { ActivityRepositoryImpl(get()) }
}

val sharedModules = listOf(networkModule, repositoryModule)

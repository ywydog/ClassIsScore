package com.classisscore.server.config;

import com.classisscore.server.websocket.ScoreWebSocketHandler;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.socket.config.annotation.EnableWebSocket;
import org.springframework.web.socket.config.annotation.WebSocketConfigurer;
import org.springframework.web.socket.config.annotation.WebSocketHandlerRegistry;

@Configuration
@EnableWebSocket
public class WebSocketConfig implements WebSocketConfigurer {

    @Bean
    public ScoreWebSocketHandler scoreWebSocketHandler() {
        return new ScoreWebSocketHandler();
    }

    @Override
    public void registerWebSocketHandlers(WebSocketHandlerRegistry registry) {
        registry.addHandler(scoreWebSocketHandler(), "/ws/scores")
                .setAllowedOrigins("*");
    }
}

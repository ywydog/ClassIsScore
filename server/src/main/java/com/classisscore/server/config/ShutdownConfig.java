package com.classisscore.server.config;

import org.springframework.boot.actuate.endpoint.annotation.Endpoint;
import org.springframework.boot.actuate.endpoint.annotation.WriteOperation;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class ShutdownConfig {

    @Bean
    public ShutdownEndpoint shutdownEndpoint() {
        return new ShutdownEndpoint();
    }

    @Endpoint(id = "shutdown")
    public static class ShutdownEndpoint {

        @WriteOperation
        public String shutdown() {
            Thread shutdownThread = new Thread(() -> {
                try {
                    Thread.sleep(500);
                } catch (InterruptedException ignored) {
                }
                System.exit(0);
            });
            shutdownThread.setDaemon(true);
            shutdownThread.start();
            return "Shutting down...";
        }
    }
}

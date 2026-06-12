package com.classisscore.server.websocket;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CopyOnWriteArrayList;

public class ScoreWebSocketHandler extends TextWebSocketHandler {

    private static final Logger logger = LoggerFactory.getLogger(ScoreWebSocketHandler.class);
    private final List<WebSocketSession> sessions = new CopyOnWriteArrayList<>();
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        sessions.add(session);
        logger.info("WebSocket 连接建立: {}", session.getId());
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        sessions.remove(session);
        logger.info("WebSocket 连接关闭: {}, status: {}", session.getId(), status);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) throws Exception {
        logger.debug("收到 WebSocket 消息: {}", message.getPayload());
    }

    @Override
    public void handleTransportError(WebSocketSession session, Throwable exception) {
        logger.error("WebSocket 传输错误: {}", session.getId(), exception);
        sessions.remove(session);
    }

    public void broadcast(Map<String, Object> message) {
        try {
            String json = objectMapper.writeValueAsString(message);
            TextMessage textMessage = new TextMessage(json);
            for (WebSocketSession session : sessions) {
                if (session.isOpen()) {
                    try {
                        session.sendMessage(textMessage);
                    } catch (IOException e) {
                        logger.error("发送 WebSocket 消息失败: {}", session.getId(), e);
                    }
                }
            }
        } catch (Exception e) {
            logger.error("序列化 WebSocket 消息失败", e);
        }
    }

    public void broadcastScoreUpdate(Long studentId, Integer scoreChange, Integer totalScore, String reason) {
        Map<String, Object> message = Map.of(
                "type", "SCORE_UPDATE",
                "studentId", studentId,
                "scoreChange", scoreChange,
                "totalScore", totalScore,
                "reason", reason != null ? reason : ""
        );
        broadcast(message);
    }

    public void broadcastLeaderboardUpdate(List<Map<String, Object>> rankings) {
        Map<String, Object> message = Map.of(
                "type", "LEADERBOARD_UPDATE",
                "rankings", rankings
        );
        broadcast(message);
    }
}

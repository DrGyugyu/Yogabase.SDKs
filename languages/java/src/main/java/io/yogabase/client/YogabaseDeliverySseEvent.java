package io.yogabase.client;

/** One delivery SSE event; {@code id} is opaque and must be echoed as Last-Event-ID. */
public record YogabaseDeliverySseEvent(String id, String event, String dataJson) {
    public YogabaseDeliverySseEvent {
        if (event == null || event.isBlank()) {
            throw new IllegalArgumentException("event must be non-blank");
        }
        if (dataJson == null) {
            throw new NullPointerException("dataJson");
        }
    }
}

package io.yogabase.client;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Spliterators;
import java.util.stream.Stream;
import java.util.stream.StreamSupport;

/** Parses delivery SSE without interpreting or persisting the protected resume ID. */
public final class YogabaseDeliverySseParser {
    private YogabaseDeliverySseParser() {
    }

    public static Stream<YogabaseDeliverySseEvent> parse(InputStream input) {
        final BufferedReader reader = new BufferedReader(new InputStreamReader(input, StandardCharsets.UTF_8));
        final Iterator<YogabaseDeliverySseEvent> iterator = new Iterator<>() {
            private YogabaseDeliverySseEvent next;
            private boolean finished;

            @Override
            public boolean hasNext() {
                if (next == null && !finished) {
                    next = readNext();
                }
                return next != null;
            }

            @Override
            public YogabaseDeliverySseEvent next() {
                if (!hasNext()) {
                    throw new java.util.NoSuchElementException();
                }
                final YogabaseDeliverySseEvent value = next;
                next = null;
                return value;
            }

            private YogabaseDeliverySseEvent readNext() {
                String id = null;
                String event = null;
                final List<String> data = new ArrayList<>();
                try {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        if (line.isEmpty()) {
                            final YogabaseDeliverySseEvent value = dispatch(id, event, data);
                            if (value != null) {
                                return value;
                            }
                            id = null;
                            event = null;
                            data.clear();
                            continue;
                        }
                        if (line.charAt(0) == ':') {
                            continue;
                        }
                        final int separator = line.indexOf(':');
                        final String field = separator < 0 ? line : line.substring(0, separator);
                        String value = separator < 0 ? "" : line.substring(separator + 1);
                        if (value.startsWith(" ")) {
                            value = value.substring(1);
                        }
                        switch (field) {
                            case "id" -> id = value;
                            case "event" -> event = value;
                            case "data" -> data.add(value);
                            default -> { }
                        }
                    }
                    finished = true;
                    return dispatch(id, event, data);
                } catch (IOException exception) {
                    finished = true;
                    throw new IllegalStateException("Unable to read delivery SSE", exception);
                }
            }
        };
        return StreamSupport.stream(Spliterators.spliteratorUnknownSize(iterator, 0), false)
            .onClose(() -> {
                try {
                    reader.close();
                } catch (IOException exception) {
                    throw new IllegalStateException("Unable to close delivery SSE", exception);
                }
            });
    }

    private static YogabaseDeliverySseEvent dispatch(String id, String event, List<String> data) {
        if (event == null || data.isEmpty()) {
            return null;
        }
        if (!"delivery-operation".equals(event)) {
            return null;
        }
        return new YogabaseDeliverySseEvent(id, event, String.join("\n", data));
    }
}

package io.stackyoga.yogabase.models;

import com.microsoft.kiota.serialization.ValuedEnum;
import java.util.Objects;

@jakarta.annotation.Generated("com.microsoft.kiota")
public enum GameServerPortProtocol implements ValuedEnum {
    Tcp("tcp"),
    Udp("udp");
    public final String value;
    GameServerPortProtocol(final String value) {
        this.value = value;
    }
    @jakarta.annotation.Nonnull
    public String getValue() { return this.value; }
    @jakarta.annotation.Nullable
    public static GameServerPortProtocol forValue(@jakarta.annotation.Nonnull final String searchValue) {
        Objects.requireNonNull(searchValue);
        switch(searchValue) {
            case "tcp": return Tcp;
            case "udp": return Udp;
            default: return null;
        }
    }
}

package io.stackyoga.yogabase;

import java.util.Arrays;
import java.util.Objects;

/**
 * The stored result of a job that succeeded with one: exactly the bytes the function returned, checked
 * against the digest the gateway sent with them before this was returned.
 *
 * @param content the result bytes
 * @param digest {@code sha256:} and sixty-four lowercase hexadecimal digits, as sent in
 *     {@code X-Yogabase-Content-Sha256} and as {@code content} hashes to
 */
public record YogabaseJobResult(byte[] content, String digest) {
    public YogabaseJobResult {
        Objects.requireNonNull(content, "content");
        Objects.requireNonNull(digest, "digest");
    }

    @Override
    public boolean equals(Object other) {
        return other instanceof YogabaseJobResult result
            && Arrays.equals(content, result.content)
            && digest.equals(result.digest);
    }

    @Override
    public int hashCode() {
        return 31 * Arrays.hashCode(content) + digest.hashCode();
    }

    @Override
    public String toString() {
        return "YogabaseJobResult[" + content.length + " bytes, " + digest + "]";
    }
}

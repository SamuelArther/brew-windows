class Ots < Formula
  desc "Share end-to-end encrypted secrets with others via a one-time URL"
  homepage "https://ots.sniptt.com"
  url "https://github.com/sniptt-official/ots/releases/download/v0.3.1/ots_0.3.1_windows_amd64.zip"
  sha256 "1d1f5a40aaea6a25bf2f31eb63db1f7ad8d46e16b0465e54a6a35538c4b39333"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sniptt-official/ots/archive/refs/tags/v0.3.1.tar.gz"
  end

  def install
    bin.install "ots.exe"
  end

  test do
    output = shell_output("#{bin}/ots --version")
    assert_match "ots version #{version}", output

    error_output = shell_output("#{bin}/ots new -x 900h 2>&1", 1)
    assert_match "Error: expiry must be less than 7 days", error_output
  end
end

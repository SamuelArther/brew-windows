class Godns < Formula
  desc "Dynamic DNS client with multiple providers support"
  homepage "https://github.com/TimothyYe/godns"
  url "https://github.com/TimothyYe/godns/releases/download/v3.4.5/godns_3.4.5_windows_amd64.tar.gz"
  sha256 "9d9c0f1b750af5e67e9bb006261958978490a6ce4b66534934b35223f888fc1e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/TimothyYe/godns/archive/refs/tags/v3.4.5.tar.gz"
  end

  def install
    bin.install "godns.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/godns -h")

    (testpath/"config.json").write "{}"
    output = shell_output("#{bin}/godns -c #{testpath}/config.json 2>&1", 1)
    assert_match "Invalid settings", output
  end
end

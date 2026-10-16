class Nexttrace < Formula
  desc "Open source visual route tracking CLI tool"
  homepage "https://www.nxtrace.org/"
  url "https://github.com/nxtrace/NTrace-core/releases/download/v1.7.4/nexttrace-tiny_windows_amd64.exe"
  sha256 "df688e32ba88c3bc9813caae8d1a04a1cd33711cd2265a12d2dbe04886688b69"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nxtrace/NTrace-core/archive/refs/tags/v1.7.4.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "nexttrace-tiny_windows_amd64.exe" => "nexttrace.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nexttrace --version 2>&1")
  end
end

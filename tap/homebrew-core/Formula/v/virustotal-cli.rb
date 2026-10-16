class VirustotalCli < Formula
  desc "Command-line interface for VirusTotal"
  homepage "https://virustotal.github.io/vt-cli/"
  url "https://github.com/VirusTotal/vt-cli/releases/download/1.3.1/Windows32.zip"
  sha256 "a9f52f8f99b03144d4245d9cd82f64850630410a5d185b04e1b5b67ca1befcdd"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/VirusTotal/vt-cli/archive/refs/tags/1.3.1.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "vt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/vt version 2>&1")
  end
end

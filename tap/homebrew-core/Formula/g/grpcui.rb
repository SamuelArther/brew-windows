class Grpcui < Formula
  desc "Interactive web UI for gRPC, along the lines of postman"
  homepage "https://github.com/fullstorydev/grpcui"
  url "https://github.com/fullstorydev/grpcui/releases/download/v1.5.4/grpcui_1.5.4_windows_x86_64.zip"
  sha256 "24defc5a97266759239f0672090540a6b6963fce5f21ec450f27d165dc652ab8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fullstorydev/grpcui/archive/refs/tags/v1.5.4.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "grpcui.exe"
  end

  test do
    host = "no.such.host.dev"
    output = shell_output("#{bin}/grpcui #{host}:999 2>&1", 1)
    assert_match(/Failed to dial target host "#{Regexp.escape(host)}:999":.*: no such host/, output)
  end
end

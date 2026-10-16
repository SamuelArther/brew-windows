class Nali < Formula
  desc "Tool for querying IP geographic information and CDN provider"
  homepage "https://github.com/zu1k/nali"
  url "https://github.com/zu1k/nali/releases/download/v0.8.1/nali-windows-amd64-v0.8.1.zip"
  sha256 "a0b1e1c157431708446b9d92ebd4690ab793012a408dfa0b76e02644d50b65c2"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zu1k/nali/archive/refs/tags/v0.8.1.tar.gz"
  end

  def install
    bin.install "nali-windows-amd64.exe" => "nali.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nali --version 2>&1")
  end
end

class Igrep < Formula
  desc "Interactive grep"
  homepage "https://github.com/konradsz/igrep"
  url "https://github.com/konradsz/igrep/releases/download/v1.3.0/igrep-v1.3.0-x86_64-pc-windows-msvc.zip"
  sha256 "8ec75744a29a9eeb1a6aaaf0080b40a185420aa75884251dfd96c7c8a787ea7c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/konradsz/igrep/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "ig.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ig --version 2>&1")
  end
end

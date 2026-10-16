class Hgrep < Formula
  desc "Grep with human-friendly search results"
  homepage "https://github.com/rhysd/hgrep"
  url "https://github.com/rhysd/hgrep/releases/download/v0.3.9/hgrep-v0.3.9-x86_64-pc-windows-msvc.zip"
  sha256 "a562dc6305f1082a8854ae971f5c5a7bfd20edf7ac835750799f3ae0c3a5a21e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rhysd/hgrep/archive/refs/tags/v0.3.9.tar.gz"
  end

  def install
    bin.install "hgrep.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hgrep --version")
    (testpath/"test").write("Hello, world!")
    system bin/"hgrep", "Hello"
  end
end

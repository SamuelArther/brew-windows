class Erdtree < Formula
  desc "Multi-threaded file-tree visualizer and disk usage analyzer"
  homepage "https://github.com/solidiquis/erdtree"
  url "https://github.com/solidiquis/erdtree/releases/download/v3.1.2/erd-v3.1.2-x86_64-pc-windows-msvc.exe"
  sha256 "df359e20e5a38384c27b98667d780c5bd5ab1ce4bac739a5fa9c5ed25511a0a1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/solidiquis/erdtree/archive/refs/tags/v3.1.2.tar.gz"
  end

  def install
    bin.install "erd-v3.1.2-x86_64-pc-windows-msvc.exe" => "erd.exe"
  end

  test do
    touch "test.txt"
    assert_match "test.txt", shell_output("#{bin}/erd")
  end
end

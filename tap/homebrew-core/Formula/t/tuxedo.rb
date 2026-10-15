class Tuxedo < Formula
  desc "Fast, keyboard-driven terminal UI for todo.txt"
  homepage "https://github.com/webstonehq/tuxedo"
  url "https://github.com/webstonehq/tuxedo/releases/download/v2026.8.1/tuxedo-v2026.8.1-x86_64-pc-windows-msvc.zip"
  sha256 "30026d4ce96617bb54b9767bde6859e8fa4d79495c945dfff769dbbe7cab99c9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/webstonehq/tuxedo/archive/refs/tags/v2026.8.1.tar.gz"
  end

  def install
    bin.install "tuxedo.exe"
  end

  test do
    touch testpath/"todo.txt"
    system bin/"tuxedo", "add", "Hello from Homebrew"
    assert_match "Hello from Homebrew", (testpath/"todo.txt").read

    assert_match version.to_s, shell_output("#{bin}/tuxedo --version")
  end
end

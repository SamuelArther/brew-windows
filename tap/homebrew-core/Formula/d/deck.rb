class Deck < Formula
  desc "Creates slide deck using Markdown and Google Slides"
  homepage "https://github.com/k1LoW/deck"
  url "https://github.com/k1LoW/deck/releases/download/v1.24.4/deck_v1.24.4_windows_amd64.tar.gz"
  sha256 "3454013349b0221a8a26388b86e2a7cf327cefa0fee8ea3a7d3d5cd19b9e3849"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/k1LoW/deck/archive/refs/tags/v1.24.4.tar.gz"
  end

  def install
    bin.install "deck.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/deck --version")
    assert_match "presentation ID is required", shell_output("#{bin}/deck export 2>&1", 1)
  end
end

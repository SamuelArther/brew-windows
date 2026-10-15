class CodeMinimap < Formula
  desc "High performance code minimap generator"
  homepage "https://github.com/wfxr/code-minimap"
  url "https://github.com/wfxr/code-minimap/releases/download/v0.6.8/code-minimap-v0.6.8-x86_64-pc-windows-gnu.zip"
  sha256 "661e19f7542c02666aad14f8b501992a7fbddfce86d66311f69ea1c85754b1bb"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wfxr/code-minimap/archive/refs/tags/v0.6.8.tar.gz"
  end

  def install
    bin.install "code-minimap.exe"
  end

  test do
    (testpath/"test.txt").write("hello world")
    assert_equal "⠉⠉⠉⠉⠉⠁\n", shell_output("#{bin}/code-minimap #{testpath}/test.txt")
  end
end

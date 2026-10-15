class Mask < Formula
  desc "CLI task runner defined by a simple markdown file"
  homepage "https://github.com/jacobdeichert/mask/"
  url "https://github.com/jacobdeichert/mask/releases/download/mask/0.11.7/mask-0.11.7-x86_64-pc-windows-msvc.zip"
  sha256 "e2240d00f1663d5a39ba7658dc9d4f8273e70c6c7f3144c1fc37eb94448e0deb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jacobdeichert/mask/archive/refs/tags/mask/0.11.7.tar.gz"
  end

  def install
    bin.install "mask.exe"
  end

  test do
    (testpath/"maskfile.md").write <<~MARKDOWN
      # Example maskfile

      ## hello (name)

      ```sh
      printf "Hello %s!" "$name"
      ```
    MARKDOWN
    assert_equal "Hello Homebrew!", shell_output("#{bin}/mask hello Homebrew")
  end
end

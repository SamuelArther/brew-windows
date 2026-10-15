class Ccat < Formula
  desc "Like cat but displays content with syntax highlighting"
  homepage "https://github.com/owenthereal/ccat"
  url "https://github.com/owenthereal/ccat/releases/download/v1.1.0/windows-amd64-1.1.0.tar.gz"
  sha256 "45b4bff0f6988c72b876cb832af37cbf21ef6f2cf9871f9e1c1afb39c22122f9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/owenthereal/ccat/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "ccat.exe"
  end

  test do
    (testpath/"test.txt").write <<~EOS
      I am a colourful cat
    EOS

    assert_match(/I am a colourful cat/, shell_output("#{bin}/ccat test.txt"))
  end
end

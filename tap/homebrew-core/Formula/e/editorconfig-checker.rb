class EditorconfigChecker < Formula
  desc "Tool to verify that your files are in harmony with your .editorconfig"
  homepage "https://editorconfig-checker.github.io/"
  url "https://github.com/editorconfig-checker/editorconfig-checker/releases/download/v4.0.2/editorconfig-checker-windows-amd64.tar.gz"
  sha256 "c3fe98bc10fc3a6f0470f5a364dffbf53861633a0108bd15123cecc66d41bf00"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/editorconfig-checker/editorconfig-checker/archive/refs/tags/v4.0.2.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "editorconfig-checker.exe"
  end

  test do
    (testpath/".editorconfig").write <<~EOS
      [version.txt]
      charset = utf-8
    EOS
    (testpath/"version.txt").write <<~EOS
      version=#{version}
    EOS

    system bin/"editorconfig-checker", testpath/"version.txt"

    assert_match version.to_s, shell_output("#{bin}/editorconfig-checker --version")
  end
end

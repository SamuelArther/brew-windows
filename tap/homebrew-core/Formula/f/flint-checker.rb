class FlintChecker < Formula
  desc "Check your project for common sources of contributor friction"
  homepage "https://github.com/pengwynn/flint"
  url "https://github.com/pengwynn/flint/releases/download/v0.0.4/flint_windows_amd64.zip"
  sha256 "dce21641ee670e0caa89b9e291ce1142f2166f37daf77b9dba24ae90d04c0b1e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pengwynn/flint/archive/refs/tags/v0.1.0.tar.gz"
  end

  def install
    bin.install "flint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/flint --version")

    shell_output("#{bin}/flint", 2)
    (testpath/"README.md").write("# Readme")
    (testpath/"CONTRIBUTING.md").write("# Contributing Guidelines")
    (testpath/"LICENSE").write("License")
    (testpath/"CHANGELOG").write("changelog")
    (testpath/"CODE_OF_CONDUCT").write("code of conduct")
    (testpath/"script").mkpath
    (testpath/"script/bootstrap").write("Bootstrap Script")
    (testpath/"script/test").write("Test Script")
    shell_output("#{bin}/flint")
  end
end

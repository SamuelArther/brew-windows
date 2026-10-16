class Jarl < Formula
  desc "Just another R linter"
  homepage "https://jarl.etiennebacher.com"
  url "https://github.com/etiennebacher/jarl/releases/download/0.6.0/jarl-x86_64-pc-windows-msvc.zip"
  sha256 "8f3faf3f99071e1c5dc84eedea4916c1714129aa511f05a8f52b4bb760c5e2e0"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/etiennebacher/jarl/archive/refs/tags/0.6.0.tar.gz"
  end

  def install
    bin.install "jarl.exe"
  end

  test do
    (testpath/"test.R").write <<~R
      # Simple R code for testing
      any(is.na(x))
    R

    assert_match version.to_s, shell_output("#{bin}/jarl --version")

    system bin/"jarl", "check", testpath/"test.R", "--fix", "--allow-no-vcs"

    formatted_content = (testpath/"test.R").read
    assert_match "anyNA(x)", formatted_content
  end
end

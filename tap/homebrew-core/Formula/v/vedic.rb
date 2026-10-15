class Vedic < Formula
  desc "Simple Sanskrit programming language"
  homepage "https://vedic-lang.github.io/"
  url "https://github.com/vedic-lang/vedic/releases/download/v2.0.6/vedic-win-x86_64.zip"
  sha256 "f9d52807ddb67169e1b84503c808fd538dace1bffe8f91cdbaffac424654bb3a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vedic-lang/vedic/archive/refs/tags/v2.0.6.tar.gz"
  end

  def install
    bin.install "vedic.exe"
  end

  test do
    # hello world in vedic
    (testpath/"hello.ved").write <<~EOS
      वद("नमस्ते विश्व!");
    EOS
    assert_match "नमस्ते विश्व!", shell_output("#{bin}/vedic hello.ved")

    assert_match version.to_s, shell_output("#{bin}/vedic --version")
  end
end

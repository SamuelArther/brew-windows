class Checkmake < Formula
  desc "Linter/analyzer for Makefiles"
  homepage "https://github.com/checkmake/checkmake"
  url "https://github.com/checkmake/checkmake/releases/download/v0.3.2/checkmake-v0.3.2.windows.amd64.exe"
  sha256 "6adf4bde2dbfe8001ecf2918abfe02f7a57191568f1d185537eccd9c59625dca"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/checkmake/checkmake/archive/refs/tags/v0.3.2.tar.gz"
  end

  def install
    bin.install "checkmake-v0.3.2.windows.amd64.exe" => "checkmake.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/checkmake --version")

    sh = testpath/"Makefile"
    sh.write <<~EOS
      clean:
      \trm bar
      \trm foo

      foo: bar
      \ttouch foo

      bar:
      \ttouch bar

      all: foo

      test:
      \t@echo test

      .PHONY: clean test
    EOS
    assert_match "phonydeclared", shell_output("#{bin}/checkmake #{sh}", 1)
  end
end

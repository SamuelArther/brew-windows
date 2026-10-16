class Air < Formula
  desc "Fast and opinionated formatter for R code"
  homepage "https://posit-dev.github.io/air/"
  url "https://github.com/posit-dev/air/releases/download/0.12.0/air-x86_64-pc-windows-msvc.zip"
  sha256 "d3edbd1dd005a9e60d1a65ae94bfc9a71491aa0144969dac80e8578155072796"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/posit-dev/air/archive/refs/tags/0.12.0.tar.gz"
  end

  conflicts_with "go-air", because: "both install binaries with the same name"

  def install
    bin.install "air.exe"
  end

  test do
    (testpath/"test.R").write <<~R
      # Simple R code for testing
      x<-1+2
      y <- 3 + 4
      print(x+y)
    R

    assert_match version.to_s, shell_output("#{bin}/air --version")

    system bin/"air", "format", testpath/"test.R"

    formatted_content = (testpath/"test.R").read
    assert_match "x <- 1 + 2", formatted_content
    assert_match "y <- 3 + 4", formatted_content
  end
end

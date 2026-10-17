class Ppss < Formula
  desc "Shell script to execute commands in parallel"
  homepage "https://github.com/louwrentius/PPSS"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/ppss/ppss-2.97.tgz"
  sha256 "25d819a97d8ca04a27907be4bfcc3151712837ea12a671f1a3c9e58bc025360f"
  license "GPL-3.0-or-later"

  deprecate! date: "2026-07-17", because: :deprecated_upstream
  disable! date: "2027-01-17", because: :deprecated_upstream

  bottle do
    sha256 cellar: :any, x86_64_windows: "84bccf0439c4610ed3e253c4e68b7ca022f4fbf1199b85f4166d9d37dbf90aaf"
  end

  def install
    bin.install "ppss"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ppss --version")
  end
end

class Marcli < Formula
  desc "Parse MARC (ISO 2709) files"
  homepage "https://github.com/hectorcorrea/marcli"
  url "https://github.com/hectorcorrea/marcli/archive/refs/tags/v1.3.1.tar.gz"
  sha256 "944c7c389d384a9515799bb688cb9ced80344709758a42c714f726957785a1c4"
  license "MIT"
  head "https://github.com/hectorcorrea/marcli.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7dbbcbfd92e0957fc54e9fe9545f421c43bfc66903b73d43a2bd26ae6e544b0a"
  end

  depends_on "go" => :build

  resource "testdata", :test do
    url "https://raw.githubusercontent.com/hectorcorrea/marcli/5434a2f85c6f03771f92ad9f0d5af5241f3385a6/data/test_1a.mrc"
    sha256 "7359455ae04b1619f3879fe39eb22ad4187fb3550510f71cb4f27693f60cf386"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/marcli"
  end

  test do
    resource("testdata").stage do
      assert_equal "=650  \\0$aCoal$xAnalysis.\n=650  \\0$aCoal$xSampling.\n\n",
      shell_output("#{bin}/marcli -file test_1a.mrc -fields 650")
    end
  end
end

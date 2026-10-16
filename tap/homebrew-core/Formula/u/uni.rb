class Uni < Formula
  desc "Unicode database query tool for the command-line"
  homepage "https://github.com/arp242/uni"
  url "https://github.com/arp242/uni/archive/refs/tags/v2.10.0.tar.gz"
  sha256 "e9208bc0028d239f9cfbb701d98b14e93eddd138ac6433c6f2f5718244ffa5bf"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f46686228219fb8ced53b049cca8f36d544ddb49977b434e6161c77ebab6e6fc"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "CLINKING BEER MUGS", shell_output("#{bin}/uni identify 🍻")
  end
end

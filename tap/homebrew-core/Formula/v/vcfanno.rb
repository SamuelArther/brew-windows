class Vcfanno < Formula
  desc "Annotate a VCF with other VCFs/BEDs/tabixed files"
  homepage "https://genomebiology.biomedcentral.com/articles/10.1186/s13059-016-0973-5"
  url "https://github.com/brentp/vcfanno/archive/refs/tags/v0.3.9.tar.gz"
  sha256 "a5119f39a70a3872067e00be2f20ac69ec8c2688fc9f4cf603b3de12c90cba4d"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "618f4569bbafa79f29b7ce0bb00cf51030de46c9615bad6d31a5e0251df65e4a"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    pkgshare.install "example"
  end

  test do
    cp_r pkgshare/"example", testpath
    output = shell_output("#{bin}/vcfanno -lua example/custom.lua example/conf.toml example/query.vcf.gz")
    assert_match version.to_s, output
    assert_match "fileformat=VCF", output
  end
end

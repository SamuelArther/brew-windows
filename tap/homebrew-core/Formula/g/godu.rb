class Godu < Formula
  desc "Simple golang utility helping to discover large files/folders"
  homepage "https://github.com/viktomas/godu"
  url "https://github.com/viktomas/godu/archive/refs/tags/v1.5.2.tar.gz"
  sha256 "09869710652969b8a9d227dea4ac33893b1f538197b101affa6a8a9aa6a50474"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0eadd5e27796f8d48809a233a46c61665ec9d21a7118f11b04763b0176ba824b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # TODO: replace ldflags: value with :goreleaser once fix PR is merged
    # https://github.com/viktomas/godu/pull/106
    system "go", "build", *std_go_args(ldflags: "-X main.goduVersion=#{version}")
  end

  test do
    # godu is an interactive TUI app
    assert_equal "#{name} #{version}\n", shell_output("#{bin}/godu -v")
  end
end

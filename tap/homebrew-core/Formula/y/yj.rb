class Yj < Formula
  desc "CLI to convert between YAML, TOML, JSON and HCL"
  homepage "https://github.com/sclevine/yj"
  url "https://github.com/sclevine/yj/archive/refs/tags/v5.1.0.tar.gz"
  sha256 "9a3e9895181d1cbd436a1b02ccf47579afacd181c73f341e697a8fe74f74f99d"
  license "Apache-2.0"
  head "https://github.com/sclevine/yj.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f6c112d8af7cf6789f896d28233d082cd4540a3f075a7073267f105da68ca229"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}")
  end

  test do
    assert_match '{"a":1}', pipe_output("#{bin}/yj -t", "a=1")
  end
end

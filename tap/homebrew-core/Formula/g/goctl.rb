class Goctl < Formula
  desc "Generates server-side and client-side code for web and RPC services"
  homepage "https://go-zero.dev"
  url "https://github.com/zeromicro/go-zero/archive/refs/tags/tools/goctl/v1.10.2.tar.gz"
  sha256 "ef0247f24122dc9531f72a992b9bd4f92469b16690d131336abb9d4ccd2a832c"
  license "MIT"
  head "https://github.com/zeromicro/go-zero.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{^tools/goctl/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9c46cb844f782af9961c3cd91f9d7153efbcf9a1583d0ceefa883d43eb75eb6b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download", "-C", "tools/goctl"
  end

  def install
    chdir "tools/goctl" do
      system "go", "build", *std_go_args, "goctl.go"
    end

    generate_completions_from_executable(bin/"goctl", shell_parameter_format: :cobra)
  end

  test do
    assert_match "goctl version #{version}", shell_output("#{bin}/goctl --version")
    # configure project path
    %w[api model rpc docker kube mongo newapi gateway].each do |f|
      mkdir_p testpath/"#{version}/#{f}"
    end
    system bin/"goctl", "template", "init", "--home=#{testpath}"
    assert_path_exists testpath/"api/main.tpl", "goctl install fail"
  end
end

class Goctl < Formula
  desc "Generates server-side and client-side code for web and RPC services"
  homepage "https://go-zero.dev"
  url "https://github.com/zeromicro/go-zero/archive/refs/tags/tools/goctl/v1.11.0.tar.gz"
  sha256 "feb2fc99a1a3d7c79fffb6277ae3f0db9c4f3a4d64b60b1175ced46dee85d215"
  license "MIT"
  head "https://github.com/zeromicro/go-zero.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{^tools/goctl/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "67c48b3a014b1537cfdb4df693c898e67f79e3ba651d28b2598bf454323d4a4a"
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

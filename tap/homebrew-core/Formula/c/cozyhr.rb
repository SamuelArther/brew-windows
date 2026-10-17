class Cozyhr < Formula
  desc "Cozy wrapper around Helm and Flux CD for local development"
  homepage "https://github.com/cozystack/cozyhr"
  url "https://github.com/cozystack/cozyhr/archive/refs/tags/v1.6.1.tar.gz"
  sha256 "2b93668c7c24ebdc0588ca15e7821de77879b883c263f2d295fba41fb9b1c05c"
  license "Apache-2.0"
  head "https://github.com/cozystack/cozyhr.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "31526adc1449fda79cdeebe87834fdda4b4a92dd475571e243ef295c74843b44"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}")
    generate_completions_from_executable(bin/"cozyhr", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cozyhr --version")
    assert_match "try setting KUBERNETES_MASTER environment variable", shell_output("#{bin}/cozyhr list 2>&1", 1)
  end
end

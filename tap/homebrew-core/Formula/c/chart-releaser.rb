class ChartReleaser < Formula
  desc "Hosting Helm Charts via GitHub Pages and Releases"
  homepage "https://github.com/helm/chart-releaser/"
  url "https://github.com/helm/chart-releaser/archive/refs/tags/v1.8.1.tar.gz"
  sha256 "288fd5a6c6b761312103f499a0e6a797f5ca11ae903f5ab88a6557712b962715"
  license "Apache-2.0"
  head "https://github.com/helm/chart-releaser.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6f4922ccf90dde1dbf9dc604a3f3e3ec27f31151176704543eb1812d4bba3154"
  end

  depends_on "go" => :build
  depends_on "helm" => :test

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/helm/chart-releaser/cr/cmd.Version=#{version}
      -X github.com/helm/chart-releaser/cr/cmd.GitCommit=#{tap.user}
      -X github.com/helm/chart-releaser/cr/cmd.BuildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:, output: bin/"cr"), "./cr"
    generate_completions_from_executable(bin/"cr", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cr version")

    system "helm", "create", "testchart"
    system bin/"cr", "package", "--package-path", testpath/"packages", testpath/"testchart"
    assert_path_exists testpath/"packages/testchart-0.1.0.tgz"
  end
end

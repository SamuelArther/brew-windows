class ChartReleaser < Formula
  desc "Hosting Helm Charts via GitHub Pages and Releases"
  homepage "https://github.com/helm/chart-releaser/"
  url "https://github.com/helm/chart-releaser/archive/refs/tags/v1.8.1.tar.gz"
  sha256 "288fd5a6c6b761312103f499a0e6a797f5ca11ae903f5ab88a6557712b962715"
  license "Apache-2.0"
  head "https://github.com/helm/chart-releaser.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4bab0fcfe4b90ffc460554a6463ff28e2c36701bfb9f78a5b65a858b6110c9dc"
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
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cr version")

    system "helm", "create", "testchart"
    system bin/"cr", "package", "--package-path", testpath/"packages", testpath/"testchart"
    assert_path_exists testpath/"packages/testchart-0.1.0.tgz"
  end
end

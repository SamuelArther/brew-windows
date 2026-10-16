class Bento < Formula
  desc "Fancy stream processing made operationally mundane"
  homepage "https://warpstreamlabs.github.io/bento/"
  url "https://github.com/warpstreamlabs/bento/archive/refs/tags/v1.22.0.tar.gz"
  sha256 "37323b09e4f5f2c4213badacdee264488bfe6c934761f62249ac16e689440336"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fd12fea6717000b1196b7ae2327a757213795e7cda15de56a94330dba5dd607d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/warpstreamlabs/bento/internal/cli.Version=#{version} -X main.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/bento"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bento --version")

    (testpath/"config.yaml").write <<~YAML
      input:
        stdin: {}#{" "}

      pipeline:
        processors:
          - mapping: root = content().uppercase()

      output:
        stdout: {}
    YAML

    assert_match "FOOBAR", pipe_output("#{bin}/bento -c #{testpath}/config.yaml", "foobar", 0)
  end
end

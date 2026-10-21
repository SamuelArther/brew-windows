class Logcli < Formula
  desc "Run LogQL queries against a Loki server"
  homepage "https://grafana.com/oss/loki"
  url "https://github.com/grafana/loki/archive/refs/tags/v3.7.9.tar.gz"
  sha256 "374cc8b5f9aab740b44b1b0d9689e69b0e6f2c0feb41942cb52d3e1c4bb7bdeb"
  license "AGPL-3.0-only"
  head "https://github.com/grafana/loki.git", branch: "main"

  livecheck do
    formula "loki"
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "36fe428fb05d7f212e2737a9a5341a22d911a7b8a0af160ad98680e806c25980"
  end

  depends_on "go" => :build
  depends_on "loki" => :test

  resource "homebrew-testdata", :test do
    url "https://raw.githubusercontent.com/grafana/loki/5c8542036609f157fee45da7efafbba72308e829/cmd/loki/loki-local-config.yaml"
    sha256 "14557cd65634314d4eec22cf1bac212f3281854156f669b61b17f2784c895ab1"
  end

  # `test do` block runs a local loki server
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/grafana/loki/pkg/util/build.Branch=main
      -X github.com/grafana/loki/pkg/util/build.Version=#{version}
      -X github.com/grafana/loki/pkg/util/build.BuildUser=#{tap.user}
      -X github.com/grafana/loki/pkg/util/build.BuildDate=#{time.iso8601}
    ]

    system "go", "build", *std_go_args(ldflags:), "./cmd/logcli"

    generate_completions_from_executable(
      bin/"logcli",
      shell_parameter_format: "--completion-script-", shells: [:bash, :zsh],
    )
  end

  test do
    port = free_port

    testpath.install resource("homebrew-testdata")
    inreplace "loki-local-config.yaml" do |s|
      s.gsub! "3100", port.to_s
      s.gsub! "/tmp", testpath
    end

    spawn formula_opt_bin("loki")/"loki", "-config.file=loki-local-config.yaml"
    sleep 3

    assert_empty shell_output("#{bin}/logcli --addr=http://localhost:#{port} labels")
  end
end

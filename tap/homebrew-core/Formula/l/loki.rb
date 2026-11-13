class Loki < Formula
  desc "Horizontally-scalable, highly-available log aggregation system"
  homepage "https://grafana.com/oss/loki"
  url "https://github.com/grafana/loki/archive/refs/tags/v3.7.10.tar.gz"
  sha256 "52e1a821de73d634a152e6737f7a4629dae830240015a2d6f244ccaaf3b13357"
  license "AGPL-3.0-only"
  compatibility_version 1
  head "https://github.com/grafana/loki.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "912e1a6bb7561aedcdad53028adf73fdea3b41d99c6685c4872956dd71d3d864"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    cd "cmd/loki" do
      system "go", "build", *std_go_args
      inreplace "loki-local-config.yaml", "/tmp", var
      etc.install "loki-local-config.yaml"
    end
  end

  service do
    run [opt_bin/"loki", "-config.file=#{etc}/loki-local-config.yaml"]
    keep_alive true
    working_dir var
    log_path var/"log/loki.log"
    error_log_path var/"log/loki.log"
  end

  test do
    port = free_port

    cp etc/"loki-local-config.yaml", testpath
    inreplace "loki-local-config.yaml" do |s|
      s.gsub! "3100", port.to_s
      s.gsub! var, testpath
    end

    spawn bin/"loki", "-config.file=loki-local-config.yaml"

    output = shell_output("curl --silent --retry 5 --retry-connrefused localhost:#{port}/metrics")
    assert_match "log_messages_total", output
  end
end

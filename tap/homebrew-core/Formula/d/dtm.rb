class Dtm < Formula
  desc "Cross-language distributed transaction manager"
  homepage "https://en.dtm.pub/"
  url "https://github.com/dtm-labs/dtm/archive/refs/tags/v1.19.0.tar.gz"
  sha256 "11340c32e810dfd463953bca0a5f5a2c41a88c35782efc2ab70cfa78733fa823"
  license "BSD-3-Clause"
  head "https://github.com/dtm-labs/dtm.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e7ba14544f587a9eaa870e34903746a6c272c1c60f410aea5670a83fe46dcf4b"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=v#{version}")
    system "go", "build", *std_go_args(output: bin/"dtm-qs"), "qs/main.go"
  end

  test do
    assert_match "dtm version: v#{version}", shell_output("#{bin}/dtm -v")

    http_port = free_port
    grpc_port = free_port

    dtm_pid = spawn({ "HTTP_PORT" => http_port.to_s, "GRPC_PORT" => grpc_port.to_s }, bin/"dtm")
    # sleep to let dtm get its wits about it
    sleep 5
    metrics_output = shell_output("curl -s localhost:#{http_port}/api/metrics")
    assert_match "# HELP dtm_server_info The information of this dtm server.", metrics_output

    all_json = JSON.parse(shell_output("curl -s localhost:#{http_port}/api/dtmsvr/all"))
    next_position = all_json["next_position"]
    transactions = all_json["transactions"]
    assert_instance_of String, next_position
    assert_instance_of Array, transactions
    assert_empty next_position
    assert_empty transactions
  ensure
    # clean up the dtm process before we leave
    Process.kill("HUP", dtm_pid)
  end
end

class Conduit < Formula
  desc "Streams data between data stores. Kafka Connect replacement. No JVM required"
  homepage "https://conduitio.github.io/"
  url "https://github.com/ConduitIO/conduit/archive/refs/tags/v0.21.1.tar.gz"
  sha256 "b865c46841ad39d477f7965e470a3e18960525916aaae40485ad9f410b42f2a0"
  license "Apache-2.0"
  head "https://github.com/ConduitIO/conduit.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4f3f827cef0831da2e107f2dcadec422d5349e263c52d0b78f671f5252d5baba"
  end

  depends_on "go" => :build

  # Fix build by reverting a half-resolved merge in pkg/lifecycle-poc/service.go
  # PR ref: https://github.com/ConduitIO/conduit/pull/3285
  patch do
    url "https://github.com/ConduitIO/conduit/commit/b3c9ccd27303573df3afe8f39bddf541c48b76b7.patch?full_index=1"
    sha256 "74f6abb271f04f5b8e874e09d055ce76dc6c4500e5dc7ff8a5b8ffbb790d5e9d"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    # Drop the test files the revert patch deletes but `patch` leaves empty
    %w[pkg/lifecycle-poc/archv2_batching_boot_test.go pkg/provisioning/boot_start_test.go].each do |f|
      rm f if File.empty?(f)
    end

    system "make", "VERSION=#{version}"
    bin.install "conduit"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/conduit --version")

    File.open("output.txt", "w") do |file|
      # redirect stdout to the file
      $stdout.reopen(file)
      pid = spawn bin/"conduit", "run", "--api.enabled", "true",
                                 "--api.grpc.address", "127.0.0.1:0",
                                 "--api.http.address", "127.0.0.1:0"
      sleep(5)
      # Kill process
      Process.kill("SIGKILL", pid)
    end
    assert_match "grpc API started", (testpath/"output.txt").read
    assert_match "http API started", (testpath/"output.txt").read
  end
end

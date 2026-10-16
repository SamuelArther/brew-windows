class Container2wasm < Formula
  desc "Container to WASM converter"
  homepage "https://ktock.github.io/container2wasm-demo/"
  url "https://github.com/container2wasm/container2wasm/archive/refs/tags/v0.8.4.tar.gz"
  sha256 "4216e148c88588924f4026d8359be35f5c861967ab8e55a733bb879cdca678e8"
  license "Apache-2.0"
  head "https://github.com/container2wasm/container2wasm.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "19cb915b810ba464b5d84b460f2adc9a83d767bc646de734c5aa35ed527a2266"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/ktock/container2wasm/version.Version=#{version}"

    %w[c2w c2w-net].each do |cmd|
      system "go", "build", *std_go_args(ldflags:, output: bin/cmd), "./cmd/#{cmd}"
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/c2w --version")
    assert_match "FROM wasi-$TARGETARCH", shell_output("#{bin}/c2w --show-dockerfile")
  end
end

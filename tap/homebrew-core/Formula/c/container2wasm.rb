class Container2wasm < Formula
  desc "Container to WASM converter"
  homepage "https://ktock.github.io/container2wasm-demo/"
  url "https://github.com/container2wasm/container2wasm/archive/refs/tags/v0.8.5.tar.gz"
  sha256 "a6846c11d8ebab255f530d70241fa28e26ce9afff5cd6a727d0fd33037d10970"
  license "Apache-2.0"
  head "https://github.com/container2wasm/container2wasm.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e6ec84cbf8e2e79c2e10a46afa928488a2d95155b4d3dc989e1d0342e269178d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/container2wasm/container2wasm/version.Version=#{version}"

    %w[c2w c2w-net].each do |cmd|
      system "go", "build", *std_go_args(ldflags:, output: bin/cmd), "./cmd/#{cmd}"
    end
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/c2w --version")
    assert_match "FROM wasi-$TARGETARCH", shell_output("#{bin}/c2w --show-dockerfile")
  end
end

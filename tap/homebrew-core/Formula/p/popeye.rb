class Popeye < Formula
  desc "Kubernetes cluster resource sanitizer"
  homepage "https://popeyecli.io"
  url "https://github.com/derailed/popeye/archive/refs/tags/v0.22.1.tar.gz"
  sha256 "f8eef3d6b9cda24f4d9bdc24620c1368cd6a749f1321a499e88b339258e01d92"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2eab8eba09d8a471ef1a04e8367255ed571dde408716cc3cb22af1a50e6158c8"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[
      -X github.com/derailed/popeye/cmd.version=#{version}
      -X github.com/derailed/popeye/cmd.commit=#{tap.user}
      -X github.com/derailed/popeye/cmd.date=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"popeye", shell_parameter_format: :cobra)
  end

  test do
    output = shell_output("#{bin}/popeye --save --out html --output-file report.html 2>&1", 1)
    # Windows words a refused connection "No connection could be made because the target machine actively refused it".
    assert_match OS.windows? ? "actively refused it" : "connect: connection refused", output

    assert_match version.to_s, shell_output("#{bin}/popeye version")
  end
end

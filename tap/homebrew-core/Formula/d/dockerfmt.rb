class Dockerfmt < Formula
  desc "Dockerfile format and parser. a modern dockfmt"
  homepage "https://github.com/reteps/dockerfmt"
  url "https://github.com/reteps/dockerfmt/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "3bbed8d06a049bac352cbb06b7a369439578896f63a6995d1ac6b52c7b7fed52"
  license "MIT"
  head "https://github.com/reteps/dockerfmt.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "60e52209be6531bdc8e628445067e0c8db5383affd0394cc63b487f0a0caf9fa"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X github.com/reteps/dockerfmt/cmd.Version=#{version}")
    generate_completions_from_executable(bin/"dockerfmt", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dockerfmt version")

    (testpath/"Dockerfile").write <<~DOCKERFILE
      from alpine:latest
      run   echo hi
    DOCKERFILE

    shell_output("#{bin}/dockerfmt --check Dockerfile", 1)
    system bin/"dockerfmt", "--write", "Dockerfile"
    assert_equal "FROM alpine:latest\nRUN echo hi\n", (testpath/"Dockerfile").read
    system bin/"dockerfmt", "--check", "Dockerfile"
  end
end

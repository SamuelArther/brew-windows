class Wait4x < Formula
  desc "Wait for a port or a service to enter the requested state"
  homepage "https://wait4x.dev"
  url "https://github.com/wait4x/wait4x/archive/refs/tags/v3.7.2.tar.gz"
  sha256 "841781dba59ce4504deba7c5fc1aa8d85e2b10a1dbcc0749b0ac019a4666a87c"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3f621d936ca23c0578319498ee91fa4d7c2ce7aff52e9be9192d9f53b038119d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "build"
    bin.install "dist/wait4x"
    generate_completions_from_executable(bin/"wait4x", shell_parameter_format: :cobra)
  end

  test do
    system bin/"wait4x", "exec", "true"
  end
end

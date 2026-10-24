class Wait4x < Formula
  desc "Wait for a port or a service to enter the requested state"
  homepage "https://wait4x.dev"
  url "https://github.com/wait4x/wait4x/archive/refs/tags/v3.7.3.tar.gz"
  sha256 "4fbb68d1387128fed8d8072761e021f67853668287c37bfc7f52d6561dbf5f3e"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "44c3a007c891f6ab6439323534ffb5ea571b9ddb1be10d9a00b8a699077eec62"
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

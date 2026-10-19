class Kubecfg < Formula
  desc "Manage complex enterprise Kubernetes environments as code"
  homepage "https://github.com/kubecfg/kubecfg"
  url "https://github.com/kubecfg/kubecfg/archive/refs/tags/v0.39.0.tar.gz"
  sha256 "31267ed218c1e4f780d67122ccaa84424e2d5d37cdc37fcde4008ed165743a8b"
  license "Apache-2.0"
  head "https://github.com/kubecfg/kubecfg.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a191de9010e61a20b246dcf89a510d54cc2fba4bf5b9e15607b4f3bddd3402a0"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "VERSION=v#{version}"
    bin.install "kubecfg"
    pkgshare.install Pathname("examples").children
    pkgshare.install Pathname("testdata").children

    generate_completions_from_executable(bin/"kubecfg", "completion", "--shell")
  end

  test do
    system bin/"kubecfg", "show", "--alpha", pkgshare/"kubecfg_test.jsonnet"
  end
end

class Kubecfg < Formula
  desc "Manage complex enterprise Kubernetes environments as code"
  homepage "https://github.com/kubecfg/kubecfg"
  url "https://github.com/kubecfg/kubecfg/archive/refs/tags/v0.43.0.tar.gz"
  sha256 "b71f195e575d46488f90042ebcd8ff947465a121b41265523393dd2c344b415e"
  license "Apache-2.0"
  head "https://github.com/kubecfg/kubecfg.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "934256a4bf3c5c6f8a2c5775565138f8c4b124e65407b17c4e558613dec5626a"
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

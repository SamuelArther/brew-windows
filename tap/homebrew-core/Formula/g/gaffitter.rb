class Gaffitter < Formula
  desc "Efficiently fit files/folders to fixed size volumes (like DVDs)"
  homepage "https://gaffitter.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/gaffitter/gaffitter/1.0.0/gaffitter-1.0.0.tar.gz"
  sha256 "c85d33bdc6c0875a7144b540a7cce3e78e7c23d2ead0489327625549c3ab23ee"
  license "GPL-3.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/gaffitter-1.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "270a3179f9a53e345b3fe6327226c2a9c67316aa9c55b12b2d5e24fdbd63014a"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    # Workaround to build with CMake 4
    args = %w[-DCMAKE_POLICY_VERSION_MINIMUM=3.5]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    system bin/"fit", "-t", "10m", "--show-size", testpath
  end
end

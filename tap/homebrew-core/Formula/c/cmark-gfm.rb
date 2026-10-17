class CmarkGfm < Formula
  desc "C implementation of GitHub Flavored Markdown"
  homepage "https://github.com/github/cmark-gfm"
  url "https://github.com/github/cmark-gfm/archive/refs/tags/0.29.0.gfm.13.tar.gz"
  version "0.29.0.gfm.13"
  sha256 "5abc61798ebd9de5660bc076443c07abad2b8d15dbc11094a3a79644b8ad243a"
  license "BSD-2-Clause"

  no_autobump! because: :incompatible_version_format

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cmark-gfm-0.29.0.gfm.13"
    sha256 cellar: :any, x86_64_windows: "ff4b325cfe40c27101bd4704502ce83ac22724904fe4fa3f19a2b96b9ef67cd5"
  end

  depends_on "cmake" => :build
  uses_from_macos "python" => :build

  deny_network_access!

  def install
    args = %W[
      -DCMAKE_INSTALL_RPATH=#{rpath}
    ]
    # Workaround to build with CMake 4
    args << "-DCMAKE_POLICY_VERSION_MINIMUM=3.5"
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    output = pipe_output("#{bin}/cmark-gfm --extension autolink", "https://brew.sh")
    assert_equal '<p><a href="https://brew.sh">https://brew.sh</a></p>', output.chomp
  end
end

class Grantlee < Formula
  desc "Libraries for text templating with Qt"
  homepage "https://github.com/steveire/grantlee"
  url "https://github.com/steveire/grantlee/releases/download/v5.3.1/grantlee-5.3.1.tar.gz"
  sha256 "ba288ae9ed37ec0c3622ceb40ae1f7e1e6b2ea89216ad8587f0863d64be24f06"
  license "LGPL-2.1-or-later"
  head "https://github.com/steveire/grantlee.git", branch: "master"

  # From https://steveire.wordpress.com/2022/11/11/grantlee-version-5-3-1-now-available/
  # > The continuation of Grantlee for Qt 6 is happening as KTextTemplate
  #
  # Just deprecating with replacement message due to incompatible API.
  deprecate! date: "2025-04-03", because: :unmaintained, replacement_formula: "ktexttemplate"
  disable! date: "2026-04-03", because: :unmaintained, replacement_formula: "ktexttemplate"

  depends_on "cmake" => [:build, :test]
  depends_on "doxygen" => :build
  depends_on "graphviz" => :build

  depends_on "qtbase"
  depends_on "qtdeclarative"

  patch do
    url "https://github.com/steveire/grantlee/commit/1efeb1cb61947e69b8c99ddbfc5b75cd27013a87.patch?full_index=1"
    sha256 "6c5fa321c5df2b970ec2873df610ec43dd2d50977cb0a104d0d7c4ecb90621c2"
    type :unofficial
    resolves "https://github.com/steveire/grantlee/pull/88"
  end

  def install
    system "cmake", "-S", ".", "-B", "build", "-DGRANTLEE_BUILD_WITH_QT6=TRUE", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    system "cmake", "--build", "build", "--target", "docs"
    (pkgshare/"doc").install Dir["build/apidox/*"]

    pkgshare.install "examples"
  end

  test do
    system "cmake", (pkgshare/"examples/codegen"), "-DGRANTLEE_BUILD_WITH_QT6=TRUE", *std_cmake_args
    system "cmake", "--build", "."
  end
end

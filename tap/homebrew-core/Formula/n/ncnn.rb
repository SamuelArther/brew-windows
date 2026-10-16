class Ncnn < Formula
  desc "High-performance neural network inference framework"
  homepage "https://github.com/Tencent/ncnn"
  url "https://github.com/Tencent/ncnn/releases/download/20260526/ncnn-20260526-windows-vs2015-shared.zip"
  sha256 "105db0ed4852f382daf9d4468a2d326253b19c8081437235fd1669da9792942e"
  license "BSD-3-Clause"
  revision 6

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Tencent/ncnn/archive/refs/tags/20260526.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/x64/bin/caffe2ncnn.exe", "caffe2ncnn"
    windows_exe "libexec/x64/bin/darknet2ncnn.exe", "darknet2ncnn"
    windows_exe "libexec/x64/bin/mxnet2ncnn.exe", "mxnet2ncnn"
    windows_exe "libexec/x64/bin/ncnn2int8.exe", "ncnn2int8"
    windows_exe "libexec/x64/bin/ncnn2mem.exe", "ncnn2mem"
    windows_exe "libexec/x64/bin/ncnn2table.exe", "ncnn2table"
    windows_exe "libexec/x64/bin/ncnnmerge.exe", "ncnnmerge"
    windows_exe "libexec/x64/bin/ncnnoptimize.exe", "ncnnoptimize"
  end

  test do
    assert_path_exists bin/"caffe2ncnn.exe"
  end
end

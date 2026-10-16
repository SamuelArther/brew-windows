class Apptainer < Formula
  desc "Application container platform that runs SIF and Docker images in WSL 2"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "apptainer", "apptainer"
    windows_exe "libexec/wintools.exe", "singularity", "singularity"
    windows_exe "libexec/wintools.exe", "run-singularity", "run-singularity"
  end

  def caveats
    <<~EOS
      Running containers (exec, run, shell) needs WSL 2:
        wsl --install --no-distribution
      Each image is imported once as the WSL distribution apptainer-<id>;
      `apptainer cache clean` removes them again.
    EOS
  end

  test do
    assert_match(/There are [0-9]+ container file/, shell_output("#{bin}/apptainer cache list"))
    assert_match "apptainer version", shell_output("#{bin}/apptainer --version")
    deffile = "bootstrap: docker\nfrom: alpine\n"
    header = ["#!/usr/bin/env run-singularity\n", "SIF_MAGIC", "01", "02", "0123456789abcdef",
              0, 0, 0, 1, 128, 585, 713, deffile.bytesize].pack("a32a10a3a3a16q<8")
    descriptor = [0x4001, 1, 1, 0x10000001, 0, 713, deffile.bytesize, deffile.bytesize,
                  0, 0, 0, 0, "", ""].pack("l<CL<L<L<q<7a128a384")
    (testpath/"test.sif").binwrite header + descriptor + deffile
    assert_match "Def.FILE", shell_output("#{bin}/apptainer sif list #{testpath}/test.sif")
    assert_equal deffile, shell_output("#{bin}/apptainer sif dump 1 #{testpath}/test.sif")
    assert_match "mksquashfs", shell_output("#{bin}/apptainer build test2.sif test.def 2>&1", 1)
  end
end

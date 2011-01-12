/**
 * Maven and Sonar plugin for .Net
 * Copyright (C) 2010 Jose Chillan and Alexandre Victoor
 * mailto: jose.chillan@codehaus.org or alexvictoor@codehaus.org
 *
 * Sonar is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * Sonar is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Sonar; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

package org.apache.maven.dotnet;

import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.StringReader;

import org.apache.maven.dotnet.commons.project.VisualStudioProject;
import org.apache.maven.dotnet.commons.project.VisualStudioSolution;
import org.apache.maven.plugin.MojoExecutionException;
import org.apache.maven.plugin.MojoFailureException;
import org.codehaus.plexus.archiver.AbstractArchiver;
import org.codehaus.plexus.archiver.ArchiverException;
import org.codehaus.plexus.archiver.zip.ZipArchiver;
import org.codehaus.plexus.util.DirectoryWalkListener;
import org.codehaus.plexus.util.DirectoryWalker;
import org.codehaus.plexus.util.FileUtils;

/**
 * Package in distributable format SLN (zip)
 * 
 * @goal package
 * @phase package
 * @description package a .Net project or solution
 * @author Mathias COCHERIL Jun 23, 2010
 */
public class PackMojo extends AbstractDotNetMojo {

    
  final static String TARGET_DIR_NAME = "target",
  WIZ_ARCHIVE_TYPE = "sln", 
  WINDOWS_FILE_SEP = "\\",
  UNIX_FILE_SEP = "/",
  PACK_TO_DIR_ATTR = " to-dir=",
  PACK_FILES_ATTR = " files=",
  DOTNET_DIR_NAME = "dotnet",
  PROP_PREFIX = DOTNET_DIR_NAME + ".", 
  PROP_PACK_FILES = PROP_PREFIX + "pack.files", ARCHIVE_BASE_NAME = "example", ARCHIVE_EXTENSION = WIZ_ARCHIVE_TYPE,
  DEFAULT_PACK_FILES_PARTERN = "files=.\\*.* to-dir=.";

  final static int DEFAULT_FILE_PERMISSIONS = 0644;

  private String packFile = DEFAULT_PACK_FILES_PARTERN;

  private File tmpDir;
  private File archive;

  @Override
  protected void executeProject(VisualStudioProject visualProject) throws MojoExecutionException, MojoFailureException {
    makeTmpDir();
    createArchive();
  }

  @Override
  protected void executeSolution(VisualStudioSolution visualSolution) throws MojoExecutionException, MojoFailureException {
  }

  @Override
  public void execute() throws MojoExecutionException, MojoFailureException {

    String packFiles = getProperty(PROP_PACK_FILES);
    if (packFiles != null) {
      this.packFile = packFiles;
    }
    makeTmpDir();
    createArchive();
    attachArchiveToProject();
  }

  private void attachArchiveToProject() throws MojoExecutionException {
    project.getArtifact().setFile(archive);
  }

  private void makeTmpDir() throws MojoExecutionException {
    tmpDir = new File(project.getBasedir().getPath() + File.separator + TARGET_DIR_NAME);
    if (tmpDir.exists())
    {
      try {
        getLog().info("Cleaning " + tmpDir);
        FileUtils.cleanDirectory(tmpDir);
      } catch (IOException e) {
        throw new MojoExecutionException("Could not clean directory " + tmpDir, e);
      }
    }
    else if ( !tmpDir.mkdirs()) {
      throw new MojoExecutionException("Could not create directory " + tmpDir);
    }
  }

  private void createArchive() throws MojoExecutionException {
    getLog().info("Create archive ");
    archive = new File(tmpDir + File.separator + project.getArtifact().getArtifactId() + "-" + project.getArtifact().getVersion() + "."
        + ARCHIVE_EXTENSION);
    try {
      ZipArchiver zip = new ZipArchiver();
      zip.setDestFile(archive);
      doPackFiles(zip);
      zip.createArchive();

    } catch (ArchiverException ae) {
      throw new MojoExecutionException("Could not archive directory " + tmpDir, ae);
    } catch (IOException ioe) {
      throw new MojoExecutionException("I/O problem in " + tmpDir, ioe);
    }
  }

  //
  // Manage dotnet.pack.files property
  // 
  // Pattern example : <dotnet.pack.files>
  //                       files=${basedir}/../**/*.idl to-dir=idl/database
  //                       files=**/changes.xml to-dir=xml
  //                       files=../../**/*.xml to-dir=xml
  //                   </dotnet.pack.files>
  //
  //  @param zipPack
  //          the current zip package
  //
  void doPackFiles(ZipArchiver zipPack) throws IOException, MojoExecutionException {

    BufferedReader br = new BufferedReader(new StringReader(packFile));
    String line;
    while ((line = br.readLine()) != null) {
      line = line.trim().replace('\t', ' ').replace(WINDOWS_FILE_SEP, File.separator).replace(UNIX_FILE_SEP, File.separator);

      while (true) {
        if (line.length() == 0) {
          break;
        }
        line = " " + line + " ";

        int filesPos = line.indexOf(PACK_FILES_ATTR);
        if (filesPos >= 0) {
          int toDirPos = line.indexOf(PACK_TO_DIR_ATTR);
          if (toDirPos < 0) {
            throw new MojoExecutionException("could not find " + PACK_TO_DIR_ATTR);
          }
          getLog().info("parsing " + line);
          int endFilesPos = line.indexOf(" ", filesPos + PACK_FILES_ATTR.length());
          int endToDirPos = line.indexOf(" ", toDirPos + PACK_TO_DIR_ATTR.length());
          String source = line.substring(filesPos + PACK_FILES_ATTR.length(), endFilesPos);
          if (source.length() == 0) {
            throw new MojoExecutionException("bad copy source " + PACK_FILES_ATTR);
          }

          String targetDir = line.substring(toDirPos + PACK_TO_DIR_ATTR.length(), endToDirPos);
          if (targetDir.length() == 0) {
            throw new MojoExecutionException("bad copy target " + PACK_FILES_ATTR);
          }

          String basedir = ".", includePattern = "";
          int starPos = source.indexOf('*');
          if (starPos < 0) {
            starPos = source.length() - 1;
          }
          int lastSlashPos = source.lastIndexOf(File.separator, starPos);
          if (lastSlashPos > 0) {
            basedir = source.substring(0, lastSlashPos);
            includePattern = source.substring(lastSlashPos + 1);
          } else if (lastSlashPos == 0) {
            basedir = File.separator;
            includePattern = source.substring(lastSlashPos + 1);
          } else {
            includePattern = source;
          }

          CopyFileCollector copyCollector = new CopyFileCollector(zipPack, targetDir);

          DirectoryWalker dw = new DirectoryWalker();
          dw.setBaseDir(new File(basedir));
          dw.addInclude(includePattern);
          dw.addDirectoryWalkListener(copyCollector);
          try {
            dw.scan();
          } catch (IllegalStateException e) {
            throw new MojoExecutionException(basedir + " : " + e.getMessage(), e);
          }
          line = line.substring(endToDirPos);
        } else {
          break;
        }
      }
    }
  }

  private class CopyFileCollector implements DirectoryWalkListener {
    
    CopyFileCollector(AbstractArchiver archiver, String target) {
      this.archiver = archiver;
      this.target = target;
      if (this.target.endsWith(File.separator) && this.target.length() > 1)
      {
        this.target = this.target.substring(0, this.target.length() - 1);
      }
    }

    public void directoryWalkStarting(File baseDir) {
      this.baseDir = baseDir.getPath();
    }

    public void directoryWalkStep(int percentage, File file) {
      if (error != null) {
        return;
      }
      String src = file.getPath();
      int permissions = DEFAULT_FILE_PERMISSIONS;
      if (src.startsWith(baseDir)) {
        try {
          String newpath = target + file.getPath().substring(baseDir.length());
          if (newpath.startsWith(File.separator)) {
            newpath = newpath.substring(1);
          }
          getLog().info("adding file " + src + " -> " + newpath);
          archiver.addFile(file, newpath, permissions);

        } catch (ArchiverException ae) {
          getLog().info(ae);
          error = ae.toString();
        } catch (StringIndexOutOfBoundsException sa) {
          getLog().info(sa);
          error = sa.toString();
        }
      } else {
        getLog().info("bad path to " + file);
      }
    }

    public void directoryWalkFinished() {
    }

    private String baseDir;
    private String target;
    private String error;
    private AbstractArchiver archiver;

    @Override
    public void debug(String arg0) {
      getLog().debug(arg0);
    }
  }
}
